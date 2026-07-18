-- You need to login using the Azure Active Directory Admin account for your Azure SQL Server Instance
-- You must login using Microsoft Entra MFA / Microsoft Entra ID – Universal with MFA support
-- This script will add the users listed below to your Azure SQL Server Instance
-- and assign them to the db_datareader, db_datawriter, and db_ddladmin roles in the database
-- You will need to run this script in the context of the database you want to add the users to
-- Replace the <user principal name> with the User Principal Name for each user listed below
-- The User principal name can be found in the Microsoft Entra ID Users listing in your Microsoft Entra ID Tenant
-- In the Overview tab for each user
-- It is in the format of <user>@<tenant>.onmicrosoft.com
-- Example: jon001sox@gmail.com#EXT#@<your domain>.onmicrosoft.com


-- Jonathan Franck
CREATE USER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];


-- Max Eringros
CREATE USER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com];


-- Jessica Pratt
CREATE USER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com];


-- Nancy Forero
CREATE USER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com];


-- Joseph Ficara
CREATE USER [jficarainstructor2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [jficarainstructor2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [<user principal name>];
ALTER ROLE db_ddladmin ADD MEMBER [jficarainstructor2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];

GO
