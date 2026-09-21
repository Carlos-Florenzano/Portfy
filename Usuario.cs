using System;

    public class Usuario
    {

        public int id { get; set; }
        public string nome { get; set; }
        public double salario_mensal { get; set; }
        public DateTime data_recebimento { get; set; }

        public Usuario(int id, string nome, double salario_mensal, DateTime data_recebimento)
        {
            id = id;
            nome = nome;
            salario_mensal = salario_mensal;
            data_recebimento = data_recebimento;
        }

        public void atualizar_salario(double novo_salario)
        {
            salario_mensal = novo_salario;
        }

        public double renda_disponivel(double total_despesas)
        {
            return salario_mensal - total_despesas;
        }

    }
