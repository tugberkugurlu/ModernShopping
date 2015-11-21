#!/bin/bash

configuration=RELEASE
scriptsDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
artifactsDir=${scriptsDir%%/}/artifacts
outputDir=${artifactsDir%%/}/apps
projectDirectory=${scriptsDir%%/}/src/ModernShopping.Auth
projectFilePath=${projectDirectory%%/}/project.json

dnu restore $projectDirectory
dnu build $projectFilePath --configuration $configuration --out $artifactsDir
dnu publish $projectDirectory --configuration $configuration --out $outputDir --runtime active