param(
    [string]$BaseUrl = "http://85.111.1.49:57005",
    [string]$OrgCode = "7374953",
    [string]$Username = "Admin",
    [string]$Password = "2009Bfm"
)

$logs = Join-Path -Path (Get-Location) -ChildPath "scripts/logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$logFile = Join-Path $logs ("koza-auth-java-{0}.log" -f (Get-Date -Format "yyyyMMdd-HHmmss"))

$payload = @"
{
  ""orgCode"": ""$OrgCode"",
  ""userName"": ""$Username"",
  ""userPassword"": ""$Password""
}
"@

$code = @"
import okhttp3.*;

public class KozaAuthTest {
  public static void main(String[] args) throws Exception {
    OkHttpClient client = new OkHttpClient().newBuilder().build();
    MediaType mediaType = MediaType.parse("application/json");
    RequestBody body = RequestBody.create(mediaType, \""$payload\"");
    Request request = new Request.Builder()
      .url(\""$BaseUrl/Yetki/Giris.do\"\")
      .method(\""POST\"", body)
      .addHeader(\""Content-Type\"", \""application/json\"\")
      .build();
    Response response = client.newCall(request).execute();
    System.out.println(response.code());
    System.out.println(response.body().string());
  }
}
"@

Set-Content -Path (Join-Path $logs "KozaAuthTest.java") -Value $code -Encoding UTF8
Set-Content -Path $logFile -Value $code -Encoding UTF8
Write-Host "Java snippet written to $logFile (and KozaAuthTest.java) with provided credentials."
