FROM microsoft/aspnet:1.0.0-beta8

WORKDIR /approot/src/ModernShopping.Auth
RUN dnu commands install Microsoft.Dnx.Watcher 1.0.0-beta8
RUN chmod +x /root/.dnx/bin/dnx-watch
RUN dnu restore

ENV PATH $PATH:/root/.dnx/bin

EXPOSE 44300
ENTRYPOINT ["dnx-watch", "web"]