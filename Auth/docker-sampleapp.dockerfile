FROM microsoft/aspnet:1.0.0-rc1-update1

COPY ./src/ModernShopping.Auth.SampleApp/project.json /app/
WORKDIR /app
RUN ["dnu", "restore"]
COPY ./src/ModernShopping.Auth.SampleApp /app

EXPOSE 5001
ENTRYPOINT ["dnx", "web"]