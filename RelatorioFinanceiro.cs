public class RelatorioFinanceiro
{
    public RelatorioFinanceiro() {}
    public RelatorioFinanceiro(string cliente, int valores, string tipo_invest, string nome_invest, double valor_invest, int ativos)
    {
        Cliente = cliente; //Ver isso sobre o sqlite
        Valores = valores;
        Tipo_Invest = tipo_invest; // Renda Fixa e Renda Variável
        Nome_Invest = nome_invest; // para CDB, LCA, Tesouro Direto, Poupança e outros
        Valor_Invest = valor_invest;
        Ativos = ativos;
    }
   
    public string Cliente {get; set;} = "";
    public int Valores {get; set;}
    public string Tipo_Invest {get; set;} = "";
    public string Nome_Invest {get; set;} = "";
    public double Valor_Invest {get; set;}
    public int Ativos {get; set;}

// Quando surgir novas informações, irei adicionar novos métodos e/ou modifica-los
}