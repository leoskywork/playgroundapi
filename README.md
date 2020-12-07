# Playground api

Providing backend api for testing/learning purpose, powered by asp.net.

## Project LeoMaster6

Build bits will be copied to debug folder(.../api/) for testing(localhost:84/api) first, and may not be put to prod folder.

* integrate with log4net.

## todo

* [x] make logger.Info(...) work
* [ ] multiple get methods not working??
    * [x] get result - invalid json format??
* [ ] a method to return history clipboard
* [ ] support authorization

## error fix

* how to fix UnauthorizedAccessException?
  > grant proper permissions to user IIS_IUSRS on that folder
* invalid hostname error when send http request by local IP address
  > working: get http://localhost:57005/mock-yf/
  > not working: get http://192.168.2.5:57005/mock-yf/
  > fix: https://blog.csdn.net/qianxing111/article/details/79884527
    * modify applicationhost.config file
	* close VS, then start with admin permission