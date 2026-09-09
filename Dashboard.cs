public class Dashboard
{
    public Dashboard() {}
    public Dashboard(string cliente, int saldo) // Irei acertar os parametros e os métodos futuramente
    {
        Cliente = cliente;
        Saldo = saldo;
    }
   
    public string Cliente {get; set;} = "";
    public int Saldo {get; set;}

// Quando surgir novas informações, irei adicionar novos métodos e/ou modifica-los
}