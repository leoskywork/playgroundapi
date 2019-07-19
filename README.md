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
