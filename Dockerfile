FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
ARG PROJECT
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked dotnet publish ${PROJECT} -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet"]
