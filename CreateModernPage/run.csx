using System.Net;

private const string ADMIN_USER_CONFIG_KEY = "SharePointAdminUser"; 
private const string ADMIN_PASSWORD_CONFIG_KEY = "SharePointAdminPassword"; 

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    // Collect site/page details from request body.
    var pci = await req.Content.ReadAsAsync<PageCreationInformation>(); 
    log.Info($"Received siteUrl={pci.siteUrl}, pageName={pci.pageName}, pageText={pci.pageText}"); 

    if (siteUrl.Contains("www.contoso.com")) 
    { 
        // N.B. the “www.contoso.com” URL indicates the local workbench in SPFx.. 
        return req.CreateResponse(HttpStatusCode.BadRequest, "Error: please run in the context of a real SharePoint site, not the local workbench. We need this to know which site to create the page in!"); 
    } 

    // Fetch auth credentials from config - N.B. consider use of app authentication for production code!
    string adminUserName = System.Environment.GetEnvironmentVariable(ADMIN_USER_CONFIG_KEY, EnvironmentVariableTarget.Process); 
    string adminPassword = System.Environment.GetEnvironmentVariable(ADMIN_PASSWORD_CONFIG_KEY, EnvironmentVariableTarget.Process);
    
    log.Info($"Will attempt to authenticate to SharePoint with username {adminUserName}");

    // Auth to SharePoint and get ClientContext.
    ClientContext ctx = 
        new OfficeDevPnP.Core.AuthenticationManager().GetSharePointOnlineAuthenticatedContextTenant(siteUrl, adminUserName, adminPassword);
    Site site = ctx.Site;
    ctx.Load(site);
    ctx.ExecuteQueryRetry();

    log.Info($"Successfully authenticated to site {ctx.Url}.");
    log.Info($"Will attempt to create page with name {pci.PageName}");

    ClientSidePage page = new ClientSidePage(ctx);
    ClientSideText cstxt = new ClientSideText() { Text = pageText };
    page.AddControl(cstxt, 0);

    // Page will be created if it doesn't exist, otherwise overwritten if it does.
    page.Save(pci.PageName);

    return pci.PageName == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass site URL, page name and page text in request body!")
        : req.CreateResponse(HttpStatusCode.OK, "Created page " + pci.PageName);
}

public sealed class PageCreationInformation 
{
    public string SiteUrl { get; set; }
    public string PageName { get; set; }
    public string PageText { get; set; }
}