#!/bin/bash

sudo docker rm $(sudo docker ps -a --filter "name=ModernShopping-auth" --filter "name=ModernShopping-auth-mongo" -q)