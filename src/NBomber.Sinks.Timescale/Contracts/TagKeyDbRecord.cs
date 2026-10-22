using System.ComponentModel.DataAnnotations.Schema;
using NBomber.Sinks.Timescale.DAL;

namespace NBomber.Sinks.Timescale.Contracts;

[Table(TableNames.SessionTagKeysTable)]
internal class TagKeyDbRecord
{
    [Column(ColumnNames.TagKey)] public string Key { get; set; } = string.Empty;
    [Column(ColumnNames.TagValues)] public string[] Values { get; set; } = [];
    [Column(ColumnNames.Created)] public DateTime Created { get; set; }
    [Column(ColumnNames.LastUpdated)] public DateTime LastUpdated { get; set; }
}
