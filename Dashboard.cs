public class Dashboard
{
    public Dashboard() {}
    public Dashboard(string cliente, int saldo)
    {
        Cliente = cliente;
        Saldo = saldo;
    }
   
    public string Cliente {get; set;} = "";
    public int Saldo {get; set;}

}