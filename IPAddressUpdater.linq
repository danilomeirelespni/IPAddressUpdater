<Query Kind="Program">
  <NuGetReference>Microsoft.PowerShell.SDK</NuGetReference>
  <Namespace>System.Collections.ObjectModel</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.NetworkInformation</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Management.Automation</Namespace>
</Query>

public static class ScriptConfig
{
	public static string HostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
	public static string SeoDockerPath = @"C:\Users\dmeireles\Desktop\seo_docker";	
	public static string LocalQaFilePath = $@"{SeoDockerPath}\localQa.conf";
}

void Main()
{
	var ip = GetCurrentIp();
	UpdateHostFileIpAddress(ip);
	UpdateLocalQaFileIpAddress(ip);
	DeleteDockerContainer();
	DeleteDockerImage();
	BuildDockerImage();
	RunDockerContainer();
}

private void UpdateHostFileIpAddress(string newIpAddress)
{
	var fileLines = File.ReadAllLines(ScriptConfig.HostsFilePath).ToList();
	fileLines = fileLines.Take(fileLines.Count - 1).ToList();	
	fileLines.Add($"{newIpAddress} local-qa.staples.com");
	File.WriteAllLines(ScriptConfig.HostsFilePath, fileLines);	
	var newText = File.ReadAllText(ScriptConfig.HostsFilePath);	
	newText.Dump("Updated hosts file:");
}

private void UpdateLocalQaFileIpAddress(string newIpAddress)
{
	var fileLines = File.ReadAllLines(ScriptConfig.LocalQaFilePath).ToList();
	fileLines[1] = $"        server {newIpAddress}:8151;";
	fileLines[5] = $"        server {newIpAddress}:8251;";
	fileLines[9] = $"        server {newIpAddress}:8351;";
	File.WriteAllLines(ScriptConfig.LocalQaFilePath, fileLines);
	var newText = File.ReadAllText(ScriptConfig.LocalQaFilePath);
	newText.Dump("Updated localQa.conf file:");	
}

public static string GetCurrentIp()
{
	var currentIp = Dns.GetHostAddresses(Dns.GetHostName())	
		.Where(x => x.AddressFamily == AddressFamily.InterNetwork)
		.Select(x => x.ToString())
		.Where(x => !x.StartsWith("192"))
		.Where(x => !x.StartsWith("172"))
		.Where(x => !x.StartsWith("10.0.0"))
		.FirstOrDefault();
		
	if (currentIp != null)
	{
		Console.WriteLine($"Current IP: {currentIp}");
		return currentIp;
	}
		
	Console.WriteLine($"Cannot detect the current IP.");
	throw new Exception("IP Not found!");
}

private void DeleteDockerContainer()
{
	ExecutePowerShell("docker rm $(docker stop $(docker ps -a -q --filter ancestor=local.qa --format=\"{{.ID}}\"))");
}

private void DeleteDockerImage()
{
	ExecutePowerShell("docker rmi --force $(docker images -q 'local.qa')");
}

private void BuildDockerImage()
{	
	ExecutePowerShell(@$"docker build -t local.qa:latest {ScriptConfig.SeoDockerPath}");
}

private void RunDockerContainer()
{
	ExecutePowerShell("docker run --name local-qa -p 80:80 -p 443:443 -itd local.qa");
}

public static Collection<PSObject> ExecutePowerShell(string command)
{
	Console.WriteLine($"{nameof(ExecutePowerShell)}...");
	Console.WriteLine(command);
	PowerShell powerShell = PowerShell.Create();
	powerShell.AddScript(command);
	Collection<PSObject> results = powerShell.Invoke();	
	return results;
}
