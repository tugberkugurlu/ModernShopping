## Running

`cd` into same directory as this README.md file and run `docker-compose up` (or `sudo docker-compose up`). This will get you up and running:

![](/.docs/Screenshot from 2015-12-25 06:54:02.png?raw=true) 

Then, `cd` under `./src/ModernShopping.Auth.SampleClient` and run `http-server -o` there. Follow the flow there and login with your Google account. This will get you an access_token that you can use:

![](/.docs/Screenshot from 2015-12-25 07:01:56.png?raw=true)

Finally, run the below `curl` command to complete the flow (don't forget to replace the token with one you have just obtained):

```shell
curl --header "Authorization: Bearer b62df5c016fb0942eedb8dbb1da69e22" http://localhost:5001/api/values -D -
```

You should see a `200 OK` response back with non-empty response body like below:

![](/.docs/Screenshot from 2015-12-25 06:59:06.png?raw=true)

## Settings

 - **MongoDB Connection String**: `export ModernShoppingAuth_MongoDb__ConnectionString=mongodb://172.17.0.2:27017`
 - **MongoDB Connection Database Name**: `export ModernShoppingAuth_MongoDb__DatabaseName=FancyAuthDbName`