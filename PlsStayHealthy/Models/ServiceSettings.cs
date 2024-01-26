namespace PlsStayHealthy.Models;
public class ServiceSettings
{
    public ServiceSettingsModel FakerApi { get; set; }
    public ServiceSettingsModel CatApi { get; set; }
    public ServiceSettingsModel FootballStandingsApi { get; set; }
}

public class ServiceSettingsModel
{
    public string Base { get; set; }
    public string Health { get; set; }
}
