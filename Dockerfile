FROM microsoft/dotnet:latest
COPY src /app
WORKDIR /app
RUN ["dotnet", "restore", "--configfile", "NuGet.Config"]
RUN ["dotnet", "build"]
EXPOSE 5000/tcp
ENV ASPNETCORE_URLS https://*:5000
ENTRYPOINT ["dotnet", "run", "--server.urls", "http://*:5000"]