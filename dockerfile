# Get sonarscanner for .NET Core SDK 2.2
FROM openjdk:8u212-jre-alpine3.9 AS ci-build

RUN apk add --no-cache \
        ca-certificates \
        \
        krb5-libs \
        libgcc \
        libintl \
        libssl1.1 \
        libstdc++ \
        lttng-ust \
        tzdata \
        userspace-rcu \
        zlib \
        icu-libs

RUN wget -O dotnet.tar.gz https://download.visualstudio.microsoft.com/download/pr/61b1ecb1-9561-49fb-8116-bb44b07676cb/48da56c16be882661eb79e1dc08b641c/dotnet-sdk-3.1.101-linux-musl-x64.tar.gz \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -xzf dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet.tar.gz

# Enable correct mode for dotnet watch (only mode supported in a container)
ENV DOTNET_USE_POLLING_FILE_WATCHER=true \
    NUGET_XMLDOC_MODE=skip

RUN dotnet tool install -g dotnet-sonarscanner
ENV PATH="${PATH}:/root/.dotnet/tools"

# Get your code repo files including .git
COPY .git ./.git
COPY src ./src
COPY SonarQube.Analysis.xml  /root/.dotnet/tools/.store/dotnet-sonarscanner/4.8.0/dotnet-sonarscanner/4.8.0/tools/netcoreapp3.0/any/SonarQube.Analysis.xml

# Sonarscanner
RUN dotnet-sonarscanner begin /k:SkillsFundingAgency_das-zendesk-monitor /o:"educationandskillsfundingagency" 

RUN dotnet build src 

RUN dotnet-sonarscanner end
