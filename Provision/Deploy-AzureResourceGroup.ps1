#Requires -Version 3.0
#Requires -Module AzureRM.Resources
#Requires -Module Azure.Storage

Param(
    [string] [Parameter(Mandatory=$true)] $ResourceGroupName,
    [string] [Parameter(Mandatory=$true)] $ResourceGroupLocation,

	[string] [ValidateSet('Consumption','Dedicated')][Parameter(ParameterSetName='RunArmTemplate')]$HostType,
	
	[string] [Parameter(Mandatory=$true, ParameterSetName='RunArmTemplate')] $AppName,

	[string] [ValidateSet('Standard_LRS','Standard_GRS','Standard_RAGRS')][Parameter(ParameterSetName='RunArmTemplate')] $StorageAccountType = "Standard_LRS",
	[string] [ValidateSet('Standard_LRS','Standard_GRS','Standard_RAGRS')][Parameter(ParameterSetName='RunArmTemplate')] $BadgeStorageAccountType = "Standard_LRS",	
    
	[string] [Parameter(Mandatory=$true)] $BadgesStorageAccountName,
	[string] [Parameter(ParameterSetName='RunArmTemplate')] $HostingPlanName,
	
	[switch] $NoAnonymousAccess,
    [switch] $ValidateOnly,

	# Dedicated template only parameters	
	[string] [Parameter(ParameterSetName='RunArmTemplate')][ValidateSet('Free','Shared','Basic','Standard')] $SKU = 'Free',
	[int] [Parameter(ParameterSetName='RunArmTemplate')] $WorkerSize = 1
)


$ErrorActionPreference = 'Stop'

Set-Variable ContainerName -Option Constant -Value "badges"

Set-StrictMode -Version 3

function Format-ValidationOutput {
    param ($ValidationOutput, [int] $Depth = 0)
    Set-StrictMode -Off
    return @($ValidationOutput | Where-Object { $_ -ne $null } | ForEach-Object { @('  ' * $Depth + ': ' + $_.Message) + @(Format-ValidationOutput @($_.Details) ($Depth + 1)) })
}

if($HostingPlanName -eq '') {
	$HostingPlanName = $HostType
}

if ($HostType -ne "") {

	$extraParameters = New-Object -TypeName Hashtable

	if($HostType -eq 'Dedicated') {
		$extraParameters.Add('sku', $SKU)
		$extraParameters.Add('workerSize', $WorkerSize)
	}
	$TemplateFile = Join-Path $HostType 'azuredeploy.json'

	$TemplateFile = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $TemplateFile))

	# Create or update the resource group using the specified template file and template parameters file
	New-AzureRmResourceGroup -Name $ResourceGroupName -Location $ResourceGroupLocation -Verbose -Force -ErrorAction Stop 

	if ($ValidateOnly) {

		$ErrorMessages = Format-ValidationOutput (Test-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
																					-TemplateFile $TemplateFile `
																					-appName $AppName `
																					-storageAccountType $StorageAccountType `
																					-badgeStorageAccountType $BadgeStorageAccountType `
																					-badgesStorageAccountName $BadgesStorageAccountName `
																					-hostingPlanName $HostingPlanName `
																					@extraParameters)

		if ($ErrorMessages) {
			Write-Output '', 'Validation returned the following errors:', @($ErrorMessages), '', 'Template is invalid.'
		}
		else {
			Write-Output '', 'Template is valid.'
		}
	} else {
		New-AzureRmResourceGroupDeployment  -ResourceGroupName $ResourceGroupName `
											-TemplateFile $TemplateFile `
											-appName $AppName `
											-storageAccountType $StorageAccountType `
											-badgeStorageAccountType $BadgeStorageAccountType `
											-badgesStorageAccountName $BadgesStorageAccountName `
											-hostingPlanName $HostingPlanName `
											@extraParameters `
											-Force -Verbose `
											-ErrorVariable ErrorMessages

		if ($ErrorMessages) {
			Write-Output '', 'Template deployment returned the following errors:', @(@($ErrorMessages) | ForEach-Object { $_.Exception.Message.TrimEnd("`r`n") })	
		}
	}
}

if($NoAnonymousAccess) {
	Write-Output '','skipping granting anonymous access. Badges will only be accessible with a token.', 'You can enable anonymous access later'
} else {

	# Create the container, since we can't create it using ARM 
	# We need to create the container to enable anonymous access so anyone can see the badges, otherweise it would be created when the first
	# badge is created but nobody would be able do see it.

	$storageKey = Get-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -AccountName $BadgesStorageAccountName -ErrorAction Ignore

	if($storageKey -eq $null) {
		Write-Error ('storage account ' + $BadgesStorageAccountName + ' or resource group ' + $ResourceGroupName + ' does not exist.')
	} else {

		$container = Get-AzureStorageContainer -Context $storageKey.Context -Name $ContainerName -ErrorAction Ignore

		if($container -eq $null) {
			if($ValidateOnly) {
				Write-Output '', ($storageKey.StorageAccountName + ' exists. Would have created ' + $ContainerName + ' container if not in validateonly mode')
			} else {
				New-AzureStorageContainer -Name $ContainerName -Permission Blob -Context $storageKey.Context -Verbose
				Write-Output '', ('Created ' + $ContainerName + ' container in ' + $storageKey.StorageAccountName + ' storage account.')
			}
		} else {
			Write-Output '', ($container.Name + ' container already exists. Skipping creation.')
		}
	}
} 