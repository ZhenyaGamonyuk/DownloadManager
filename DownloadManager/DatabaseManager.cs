
namespace DownloadManager
{
    static class DatabaseManager
    {
        static Database db;

        public static Database Database
        {
            get
            {
                if (db == null)
                    db = new Database();
                return db;
            }
        }
    }
}
