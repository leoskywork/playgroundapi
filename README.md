# Playground api

Providing backend api for testing/learning purpose, powered by asp.net(.net framework 4.6)

## Project LeoMaster6

* integrated with log4net
* mocking YF app api
* introspection api with persistance layer(txt file as data source)
* test
  * api version: ----- http://localhost:57005/tool
  * master web age: -- http://localhost:57005/app/age
  * mocking yf: ------ http://localhost:57005/mock-yf
  * introspection: --- http://localhost:57005/introspection
  * note api: -------- http://localhost:57005/note

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
  > working: ----- get http://localhost:57005/mock-yf/
  > not working: - get http://192.168.2.5:57005/mock-yf/
  > fix: --------- https://blog.csdn.net/qianxing111/article/details/79884527
    * modify applicationhost.config file
	* close VS, then start with admin permission
