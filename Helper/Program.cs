using System.Text.Json;

var file = "/Users/dmitrybahtenkov/RiderProjects/Letovo/OmniscanAdmin/chips.json";

var chips = JsonSerializer.Deserialize<List<Chip>>(File.ReadAllText(file));

var random = new Random();
var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

if (chips != null)
{
    foreach (var chip in chips)
    {
        var lastId = chip.Id;
        chip.Id = Random.Shared.Next(1, 400);
        chip.SerialNumber = chip.SerialNumber.Replace(lastId.ToString(), chip.Id.ToString());
    }

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    var updatedJson = JsonSerializer.Serialize(chips, options);
    File.WriteAllText(file, updatedJson);

    Console.WriteLine($"Успешно обновлено {chips.Count} чипов с новыми кодами отключения.");

    foreach (var chip in chips)
    {
        Console.WriteLine($"ID: {chip.Id}, Name: {chip.Name}, DisableCode: {chip.DisableCode}");
    }
}

public class Chip
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int StatusCode { get; set; }
    public string LastCommand { get; set; } = "";
    public DateTime LastUpdate { get; set; }
    public string SerialNumber { get; set; } = "";
    public string DisableCode { get; set; } = "";
}
