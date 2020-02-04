#!/bin/bash

# This shell script sets up and runs Sonarscanner against the zendesk codebase

cp SonarQube.Analysis.xml  /root/.dotnet/tools/.store/dotnet-sonarscanner/4.8.0/dotnet-sonarscanner/4.8.0/tools/netcoreapp3.0/any/SonarQube.Analysis.xml

dotnet-sonarscanner begin /k:"SkillsFundingAgency_das-zendesk-monitor" /o:"educationandskillsfundingagency"

# TODO: Catch build errors and stop processing
dotnet build src/SFA.DAS.Zendesk.Monitor.sln
dotnet test src/SFA.DAS.Zendesk.Monitor.UnitTests

dotnet-sonarscanner end
