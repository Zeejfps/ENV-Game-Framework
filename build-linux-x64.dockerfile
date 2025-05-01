FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /src

ARG csProj

# Copy csproj and restore as distinct layers
COPY $csProj ./
RUN dotnet restore

# Copy everything else and build AOT
COPY . ./

RUN dotnet publish $csProj -c Release -r linux-x64 \
    -p:PublishAot=true \
    -p:SelfContained=true \
    -p:StripSymbols=true \
    -o /publish

FROM scratch AS export
COPY --from=build /publish /publish