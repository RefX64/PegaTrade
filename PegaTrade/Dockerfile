FROM microsoft/dotnet:aspnetcore-runtime

WORKDIR /app

COPY . .

ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "PegaTrade.dll"]