using PUR2019.WinForms.Configuration;

namespace PUR2019.WinForms.Services;

public static class PurchaseOrderServiceFactory
{
    public static (IPurchaseOrderService Service, string SourceLabel) Create(DataSourceSettings settings)
    {
        if (settings.Mode.Equals("Database", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.OdbcConnectionString))
            {
                throw new InvalidOperationException("資料來源模式為 Database，但未設定 OdbcConnectionString。");
            }

            return (new OdbcPurchaseOrderService(settings.OdbcConnectionString), "資料來源：Database (ODBC)");
        }

        return (new InMemoryPurchaseOrderService(), "資料來源：InMemory");
    }
}
