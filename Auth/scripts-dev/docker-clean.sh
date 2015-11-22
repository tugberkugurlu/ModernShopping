#!/bin/bash

sudo docker rm $(sudo docker ps -a --filter "name=ModernShopping_auth" --filter "name=ModernShopping_auth_mongo" -q)