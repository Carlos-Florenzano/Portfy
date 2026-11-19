// Gráfico de orçamentos por categoria (donut) com ECharts.
// O componente é dono do ciclo de vida da instância: cria no mount, redimensiona
// com o container e descarta no unmount. A tela só entrega a lista de orçamentos.
import { useEffect, useRef } from 'react';
import { init, use as registrarModulosDoEcharts } from 'echarts/core';
import type { ComposeOption } from 'echarts/core';
import { PieChart } from 'echarts/charts';
import type { PieSeriesOption } from 'echarts/charts';
import { LegendComponent, TooltipComponent } from 'echarts/components';
import type { LegendComponentOption, TooltipComponentOption } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import type { Orcamento } from '../types/orcamento';
import { formatarMoeda } from './formatadores';

// Só o necessário para este gráfico entra no bundle (ECharts é modular).
// O `use` do ECharts é aliased para não colidir com o hook `use` do React.
registrarModulosDoEcharts([PieChart, TooltipComponent, LegendComponent, CanvasRenderer]);

type OpcaoDoGrafico = ComposeOption<
    PieSeriesOption | TooltipComponentOption | LegendComponentOption
>;

// Paleta em sintonia com os mockups: azul, esmeralda, âmbar e derivados.
const PALETA = ['#2563eb', '#34d399', '#fbbf24', '#60a5fa', '#a78bfa', '#f472b6', '#22d3ee'];

export function GraficoDeOrcamentos({ orcamentos }: { orcamentos: Orcamento[] }) {
    const container = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const elemento = container.current;

        if (!elemento) return;

        const grafico = init(elemento, undefined, { renderer: 'canvas' });

        const opcao: OpcaoDoGrafico = {
            color: PALETA,
            tooltip: {
                trigger: 'item',
                backgroundColor: '#181818',
                borderColor: '#262626',
                textStyle: { color: '#ededed' },
                // Valor já formatado em reais; a tela não faz conta financeira.
                valueFormatter: (valor) => formatarMoeda(Number(valor)),
            },
            legend: {
                bottom: 0,
                textStyle: { color: '#a3a3a3' },
            },
            series: [
                {
                    name: 'Limite por categoria',
                    type: 'pie',
                    radius: ['58%', '82%'],
                    center: ['50%', '44%'],
                    avoidLabelOverlap: true,
                    itemStyle: { borderColor: '#121212', borderWidth: 3 },
                    label: { show: false },
                    emphasis: {
                        label: { show: true, color: '#ededed', fontWeight: 'bold', formatter: '{b}\n{d}%' },
                    },
                    data: orcamentos.map((orcamento) => ({
                        name: orcamento.nomeCategoria,
                        value: orcamento.limiteDefinido,
                    })),
                },
            ],
        };

        grafico.setOption(opcao);

        const observador = new ResizeObserver(() => grafico.resize());
        observador.observe(elemento);

        return () => {
            observador.disconnect();
            grafico.dispose();
        };
    }, [orcamentos]);

    return (
        <div
            ref={container}
            role="img"
            aria-label="Distribuição dos orçamentos por categoria"
            className="h-72 w-full"
        />
    );
}
