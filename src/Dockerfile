# This stage is used to build the service project
ARG VERSION
FROM mcr.microsoft.com/dotnet/aspnet:$VERSION AS base

RUN apt-get update && \
    apt-get -s dist-upgrade | { grep "^Inst" || true; } | awk -F " " {'print $2'} | xargs apt-get -y install && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* && \
    addgroup dotnet && \
    adduser -system -u 1000 -group dotnet

ENV TZ=Africa/Johannesburg
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

USER dotnet
WORKDIR /app

EXPOSE 8080
EXPOSE 443

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY artifacts/api /app
ENTRYPOINT ["dotnet", "Menlo.Api.dll"]
