using MySqlConnector;

public class DataBase
{
    MySqlConnection connection = new MySqlConnection("host=192.168.100.126;Port=3306;Database=wavetune;User Id=root;Password=root;");

    public void openConnection()
    {
        if (connection.State == System.Data.ConnectionState.Closed)
            connection.Open();
    }

    public void closeConnection()
    {
        if (connection.State == System.Data.ConnectionState.Open)
            connection.Close();
    }


    public MySqlConnection getConnection()
    {
        return connection;
    }

}
