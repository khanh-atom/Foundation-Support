@echo off
cd /d %~dp0
openfiles > NUL 2>&1
if %ERRORLEVEL% NEQ 0 (
	set "errorMessage=setup-database.cmd script must be run in an elevated (admin) command prompt"
	goto error
)

mode con:cols=120 lines=5000
set ROOTPATH=%cd%
set ROOTDIR=%cd%

if "%~1" == "" goto loop

set APPNAME=%~1
set SQLSERVER=%~2
set ADDITIONAL_SQLCMD=%~3
goto main

:loop
set SQLSERVER=
set ADDITIONAL_SQLCMD=
set /p APPNAME=Enter your app name (required):
set /p SQLSERVER=Enter your SQL server name (optional, press Enter for default (.) local server):
set /p ADDITIONAL_SQLCMD=Enter your sqlcmd command (optional, press Enter for default (-E) windows auth):

set check=false
if "%APPNAME%"=="" (set check=true)
if "%check%"=="true" (
	echo Parameters missing, application name is required
	pause
	cls
	goto loop
)

:main
if "%SQLSERVER%"=="" (set SQLSERVER=.)
if "%ADDITIONAL_SQLCMD%"=="" (set ADDITIONAL_SQLCMD=-E)

cls
echo Your application name is: %APPNAME%
echo Your SQL server name is: %SQLSERVER%
echo Your SQLCMD command is: sqlcmd -S %SQLSERVER% %ADDITIONAL_SQLCMD%
timeout 15

set cms_db=%APPNAME%.Cms
set commerce_db=%APPNAME%.Commerce
set user=%cms_db%User
set password=Episerver123!
set errorMessage = "" 

cls
echo ######################################################################
echo #     Database Setup - This will take around 2 to 5 minutes         #
echo ######################################################################
echo #                                                                    #
echo #                         (  )   (   )  )                            #
echo #                          ) (   )  (  (                             #
echo #                          ( )  (    ) )                             #
echo #                          _____________                             #
echo #                         ^|_____________^| ___                      #
echo #                         ^|             ^|/ _ \                     #
echo #                         ^|             ^| ^| ^|                    #
echo #                         ^| Optimizely  ^|_^| ^|                    #
echo #                      ___^|             ^|\___/                     #
echo #                     /    \___________/    \                        #
echo #                     \_____________________/                        #
echo #                                                                    #
echo ######################################################################

echo ## Database Setup - please check the Build\Logs directory if you receive errors

md "%ROOTPATH%\Build\Logs" 2>nul

set sql=sqlcmd -S %SQLSERVER% %ADDITIONAL_SQLCMD%
echo ## %sql% ##

echo ## Dropping databases ##
echo ## Dropping databases ## > Build\Logs\Database.log
%sql% -Q "EXEC msdb.dbo.sp_delete_database_backuphistory N'%cms_db%'" >> Build\Logs\Database.log
%sql% -Q "if db_id('%cms_db%') is not null ALTER DATABASE [%cms_db%] SET SINGLE_USER WITH ROLLBACK IMMEDIATE" >> Build\Logs\Database.log
%sql% -Q "if db_id('%cms_db%') is not null DROP DATABASE [%cms_db%]" >> Build\Logs\Database.log
%sql% -Q "EXEC msdb.dbo.sp_delete_database_backuphistory N'%commerce_db%'" >> Build\Logs\Database.log
%sql% -Q "if db_id('%commerce_db%') is not null ALTER DATABASE [%commerce_db%] SET SINGLE_USER WITH ROLLBACK IMMEDIATE" >> Build\Logs\Database.log
%sql% -Q "if db_id('%commerce_db%') is not null DROP DATABASE [%commerce_db%]" >> Build\Logs\Database.log

echo ## Dropping user ##
echo ## Dropping user ## >> Build\Logs\Database.log
%sql% -Q "if exists (select loginname from master.dbo.syslogins where name = '%user%') EXEC sp_droplogin @loginame='%user%'" >> Build\Logs\Database.log

echo ## Creating CMS and Commerce databases ##
echo ## Creating CMS and Commerce databases ## >> Build\Logs\Database.log
dotnet tool update EPiServer.Net.Cli --global --add-source https://nuget.optimizely.com/feed/packages.svc/
dotnet-episerver create-cms-database ".\src\Foundation\Foundation.csproj" -S "%SQLSERVER%" %ADDITIONAL_SQLCMD%  --database-name "%APPNAME%.Cms"
dotnet-episerver create-commerce-database ".\src\Foundation\Foundation.csproj" -S "%SQLSERVER%" %ADDITIONAL_SQLCMD%  --database-name "%APPNAME%.Commerce" --reuse-cms-user

echo ## Installing foundation configuration ##
echo ## Installing foundation configuration ## >> Build\Logs\Database.log
%sql% -d %commerce_db% -b -i "Build\SqlScripts\FoundationConfigurationSchema.sql" -v appname=%APPNAME% >> Build\Logs\Database.log

echo ## Installing unique coupon schema ##
echo ## Installing unique coupon schema ## >> Build\Logs\Database.log
%sql% -d %commerce_db% -b -i "Build\SqlScripts\UniqueCouponSchema.sql" >> Build\Logs\Database.log

echo ## Installing Service API CMS schema ##
echo ## Installing Service API CMS schema ## >> Build\Logs\Database.log
%sql% -d %cms_db% -b -i "Build\SqlScripts\ServiceApiCms.sql" >> Build\Logs\Database.log

echo ## Installing Service API Commerce schema ##
echo ## Installing Service API Commerce schema ## >> Build\Logs\Database.log
%sql% -d %commerce_db% -b -i "Build\SqlScripts\ServiceApiCommerce.sql" >> Build\Logs\Database.log

echo ## Database setup completed successfully! ##
echo ## Database setup completed successfully! ## >> Build\Logs\Database.log

:error
if NOT "%errorMessage%"=="" echo %errorMessage%

echo Run resetup-database.cmd to resetup database only
echo @echo off > resetup-database.cmd
echo cls >> resetup-database.cmd
echo echo ###################################################################### >> resetup-database.cmd
echo echo #           Rebuild the current database from default              # >> resetup-database.cmd
echo echo ###################################################################### >> resetup-database.cmd
echo echo #                                                                    # >> resetup-database.cmd
echo echo #       NOTE: This will **DROP** the existing databases             # >> resetup-database.cmd
echo echo #             and resetup so use with caution!!                      # >> resetup-database.cmd
echo echo #                                                                    # >> resetup-database.cmd
echo echo #       Crtl+C NOW if you are unsure!                                # >> resetup-database.cmd
echo echo #                                                                    # >> resetup-database.cmd
echo echo ###################################################################### >> resetup-database.cmd
echo pause >> resetup-database.cmd
echo setup-database %APPNAME% %SQLSERVER% "%ADDITIONAL_SQLCMD%" >> resetup-database.cmd

pause
