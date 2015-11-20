FROM microsoft/aspnet:1.0.0-beta8

ADD ./src /app
WORKDIR /app/ModernShopping.Auth
RUN ["dnu", "restore"]

EXPOSE 44300
ENTRYPOINT ["dnx", "web"]