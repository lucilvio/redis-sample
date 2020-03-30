namespace Lucilvio.Redis.Sample.Controllers
{
    public class ListItem
    {
        private ListItem() { }

        public ListItem(string valor, string texto)
        {
            this.Valor = valor;
            this.Texto = texto;
        }

        public string Texto { get; set; }
        public string Valor { get; set; }
    }
}