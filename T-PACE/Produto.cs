namespace T_PACE
{
    public class Produto
    {
        public int id { get; set; }
        public string codigo_barras { get; set; } = string.Empty;
        public string nome { get; set; } = string.Empty;

        // Adicionado '?' para permitir valores nulos vindos do banco
        public decimal? custo { get; set; }
        public decimal preco_venda { get; set; }
        public string ncm { get; set; } = string.Empty;
        public string cest { get; set; } = string.Empty;

        // Adicionado '?' para permitir valores nulos vindos do banco
        public decimal? aliquotas_imposto { get; set; }
        public decimal quantidade { get; set; }
        public double? valor_promocional { get; set; }
        public string unidade_venda { get; set; } = string.Empty;
        public bool em_promocao { get; set; }
        public string lote { get; set; } = string.Empty;
        public string validade { get; set; } = string.Empty;

        // Adicionado '?' para permitir valores nulos vindos do banco
        public int? id_filial { get; set; }
        public decimal? quantidade_minima { get; set; }

        public string foto { get; set; } = string.Empty;
        public long? codigo_geral { get; set; }
    }
}