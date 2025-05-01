FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

RUN apt-get update && apt-get install -y clang

# Set working directory
WORKDIR /src

COPY . ./

ARG PROJECT_PATH

RUN dotnet publish "$PROJECT_PATH" \
    -c Release -r linux-x64 \
    -p:PublishAot=true \
    -p:SelfContained=true \
    -p:StripSymbols=true \
    -o /publish

FROM scratch AS export
COPY --from=build /publish /publish