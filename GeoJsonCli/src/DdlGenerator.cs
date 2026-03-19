using System.Text;

namespace GeoJsonCli;

public static class DdlGenerator
{
    public static string Generate(TableSchema schema)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE TABLE {QuoteIdentifier(schema.TableName)} (");

        for (int i = 0; i < schema.Columns.Count; i++)
        {
            var col = schema.Columns[i];
            var isLast = i == schema.Columns.Count - 1;
            var nullable = col.Nullable ? "" : " NOT NULL";
            sb.AppendLine($"    {QuoteIdentifier(col.Name)} {col.SqlType()}{nullable}{(isLast ? "" : ",")}");
        }

        sb.AppendLine(");");

        return sb.ToString();
    }

    private static string QuoteIdentifier(string name) => $"\"{name}\"";
}
