FROM microsoft/aspnet:1.0.0-rc1-update1

COPY ./src/ModernShopping.Auth/project.json /app/ModernShopping.Auth/
COPY ./src/Dnx.Identity.MongoDB/project.json /app/Dnx.Identity.MongoDB/
COPY ./NuGet.Config /app/
WORKDIR /app/
RUN ["dnu", "restore", "--parallel"]
ADD ./src /app/

EXPOSE 44300
WORKDIR /app/ModernShopping.Auth/
ENTRYPOINT ["dnx", "web"]