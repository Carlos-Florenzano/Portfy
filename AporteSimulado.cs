using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorFinanceiro
{
    public class AporteSimulado
    {
        // Propriedades
        public int Id { get; set; }
        public DateTime DataAporte { get; set; }
        public decimal ValorAportado { get; set; }
        public int OrigemSalarioId { get; set; }

        // Armazena o histórico de aportes
        private static List<AporteSimulado> listaAportes = new List<AporteSimulado>();

        // Método para registrar um aporte na simulação
        public void AlocarParaSimulacao()
        {
            listaAportes.Add(this);

            Console.WriteLine(
                $"Aporte de R$ {ValorAportado:F2} realizado em {DataAporte:dd/MM/yyyy} " +
                $"(Origem Salário ID: {OrigemSalarioId}).");
        }

        // Método para consultar o histórico de aportes
        public static List<AporteSimulado> HistoricoAportes()
        {
            return listaAportes.OrderBy(a => a.DataAporte).ToList();
        }
    }
}
