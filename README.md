# Using Azure KeyVault for OAuth2 client creds
This sample implements an Azure Function App using Managed Identity to obtain an access token to an API. Obtaining access tokens from Azure AD is [well documented](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-v2-protocols) when using regular application identities. However, use of Managed Identities is well documented only when used for [obtaining access to selected Azure services](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-can-i-use-managed-identities-for-azure-resources). This sample was developed to show how to accomplish this task for other resources, e.g. Graph API or your own API. It uses Azure KeyVault, which is one of the services accessible directly with a Managed Identity to provide a secure path from the Managed Identity to a regular identity used in typical scenarios. 

**Since writing this sample I have discovered that Managed Identities can be used directly to obtain OAuth2 access tokens to any API registered in 
AzureAD [Graph or custom](https://stackoverflow.com/questions/48013011/msi-permissions-for-graph-api/48014153#48014153). Managed Identities are 
implemented as AzureAD applications and service principals. However, they are not exposed as such through the Azure AD portal. Therefore, 
their access to Graph or custom APIs has to be configured through an API: MS Graph (or PowerShell). See the new [sample code](). Startup.cs includes the PS script**

This uses a more complex approach. I am leaving it here for a while to make sure my correction is seen by those who may have bookmarked this page.

The private key used to sign the client assertion and thus authenticate the function to Azure AD is generated
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
### Function App
1. Publish the code to your Azure App service
2. In the Platform Features, choose Identity and enable Managed Identity (system assigned)
3. You wil need to update the Application Settings with data obtained later so you may want to do the rest of the
setup using Azure portal opened in a separate browser window.
### KeyVault
1. Add (generate) a new certificate
2. In the Advanced Configuration mark the Private Key as non-exportable and disable transparency
3. Export the public key of the certificate to a local file
3. In KeyVaults Access Policies, add an Access Policy:
* Leave template empty
* Grant Key permission to Sign
* Grant Certificate permission to Get (needed to get certificate thumbprint in code)
* Select Principal, search for the name of your Function App defined above
4. If you are planning to debug the code from your local machine, add yourself as user as well, with same permissions
5. Select detail view of your new certificate. From the Key Identifier property extract the last segment (e.g. c700b73e78fd471b9ecacdd2a27a4338)
and save it as *signingKeyId* in your Function App application settings and *local.settings.json* if you plan to run the code locally
6. At the same time, set the *keyVaultName* setting to the short name of your key vault (the first segment in the uri).
### Azure AD
>**Note:** creating a Managed Identity for the Function App created service principal in the AAD controlling
the subscription owning the KeyVault (it does not show in the portal but it is there). 
However, you cannot use that application to assign to it permissions to access other APIs. You will need to create a new application.
The new application may even be defined in a different AAD tenant (as in my case).

1. Register a new application in your Azure AD
2. You do not need to set a reply url - we will only use OAuth2 Client Credentials token in this app
3. Copy the Application Id given to the app and paste it into the Function App application settings as *appId*.
4. Copy the Tenant Id from the same page and paste it as *tenantId* into Function App settings
5. Select the API Permissions you want to give to the app. This demo uses Microsfot Graph, therefore the *resourceId* in the Function App
application settings is set to *https://graph.microsoft.com*. Make sure that the permissions are granted by the AAD admin.

