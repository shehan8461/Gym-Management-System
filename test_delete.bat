@echo off
curl --digest -u "admin:metro@kandy1" -X PUT "http://192.168.1.100/ISAPI/AccessControl/UserInfo/Modify?format=json" -H "Content-Type: application/json" -d "{\"UserInfo\": {\"employeeNo\": \"44\", \"Valid\": {\"enable\": false}}}"
pause
