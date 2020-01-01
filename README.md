# Using Azure KeyVault for OAuth2 client creds
This sample implements an Azure Function App, which uses Azure KeyVault to sign OAuth2 client assertions used to obtain JWT tokens
from Azure AD. The private key used to sign the client assertion and thus authenticate the function to Azure AD is generated
in the KeyVault and never leaves that service (it is not exportable). This prevents potential credentials theft, which could occur
if the key was generated outside of the KeyVault and then deployed, read into the function code itself or used directly in the
assertion as a symmetric key would.
Using [Azure AD Managed Identities](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview), we
can ensure that only this Function App has access to the signing key in the vault and therefore protect other resources it
accesses using OAuth2 tokens.
## Execution
[The function returns](https://mrkeyvaultdemo.azurewebsites.net/api/GetAADData) a JWT token that can be used 
in the Authorization header for Microsoft Graph queries against my Azure AD demo tenant.
## Setup
### Function
1. Publish the code to your Azure App service
2. In the Platform Features, choose Identity and enable Managed Identity (system assigned)
3. You wil need to update the Application Settings with data obtained later so you may want to do the rest of the
setup using Azure portal opened in a separate browser window.
### KeyVault
1. Add (generate) a new certificate
2. In the Advanced Configuration mark the Private Key as non-exportable and disable transparency
3. In KeyVaults Access Policies, add an Access Policy:
3.1. Leave template empty
3.2. Grant Key permission to Sign
3.3. Grant Certificate permission to Get (needed to get certificate thumbprint in code)
3.4. Select Principal, search for the name of your Function App defined above
4. If you are planning to debug the code from your local machine, add yourself as user as well, with same permissions
5. Select detail view of your new certificate. From the Key Identifier property extract the last segment (e.g. c700b73e78fd471b9ecacdd2a27a4338)
and save it as *signingKeyId* in your Function App application settings and *local.settings.json* if you plan to run the code locally
6. At the same time, set the *keyVaultName* setting to the short name of your key vault (the first segment in the uri).
### Azure AD
1. Register a new application in your Azure AD
2. You do not need to set a reply url - we will only use OAuth2 Client Credentials token in this app
3. Copy the Application Id given to the app and paste it into the Function App application settings as *appId*.
4. Copy the Tenant Id from the same page and paste it as *tenantId* into Function App settings
5. Select the API Permissions you want to give to the app. This demo uses Microsfot Graph, therefore the *resourceId* in the Function App
application settings is set to *https://graph.microsoft.com*. Make sure that the permissions are granted by the AAD admin.

