##################################################################################################
## Set ASPNET_ENV variable as 'Development' unless it's already set to something
##################################################################################################

$AspNetEnvVar = [Environment]::GetEnvironmentVariable("ASPNET_ENV", [EnvironmentVariableTarget]::User)
$hasAspNetEnvVar = ($AspNetEnvVar -ne $null)
if($hasAspNetEnvVar -eq $false) {
	[Environment]::SetEnvironmentVariable("ASPNET_ENV", "Development", "User")
}
else {
	Write-Output "'ASPNET_ENV' environment variable is already set to '$AspNetEnvVar'"
}