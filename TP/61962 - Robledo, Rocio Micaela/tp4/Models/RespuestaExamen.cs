namespace tp4.Models
{
    public class RespuestaExamen
    {
        public int Id { get; set; }
        public int PreguntaId { get; set; }
        public Pregunta Pregunta { get; set; }
        public int ResultadoExamenId { get; set; }
        public ResultadoExamen ResultadoExamen { get; set; }
        public string OpcionElegida { get; set; }
        public bool EsCorrecta { get; set; }
    }
}
