#!pwsh

Set-StrictMode -Version Latest
Push-Location -Path $PSScriptRoot\..

# Run the CI workflow using act
act pull_request -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest
