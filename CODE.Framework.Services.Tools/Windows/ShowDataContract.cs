using System.Collections;
using System.Reflection;
using System.Windows.Forms;

namespace CODE.Framework.Services.Tools.Windows
{
    /// <summary>
    /// Show data contract
    /// </summary>
    public partial class ShowDataContract : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowDataContract"/> class.
        /// </summary>
        public ShowDataContract()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Launches the data contract inspection UI
        /// </summary>
        /// <param name="contract"></param>
        public static void Show(object contract)
        {
            var window = new ShowDataContract();
            window.ShowContract(contract);
            window.Show();
        }

        /// <summary>
        /// Shows the contract.
        /// </summary>
        /// <param name="contract">The contract.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Not a problem in the test environment.")]
        public void ShowContract(object contract)
        {
            try
            {
                Text = "Contract: " + contract;
                treeView1.Nodes.Clear();
                var root = treeView1.Nodes.Add(contract.ToString());

                var contracts = contract as IEnumerable;
                if (contracts != null)
                {
                    var counter2 = -1;
                    foreach (var item in contracts)
                    {
                        counter2++;
                        var newNode2 = root.Nodes.Add("[" + counter2.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
                        ShowObject(item, newNode2);
                    }
                }
                else
                {
                    ShowObject(contract, root);
                }
            }
            catch { }
        }

        /// <summary>
        /// Shows the object.
        /// </summary>
        /// <param name="contract">The contract.</param>
        /// <param name="parentNode">The parent node.</param>
        private static void ShowObject(object contract, TreeNode parentNode)
        {
            if (contract == null)
            {
                return;
            }
            var contractType = contract.GetType();
            var properties = contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                    if (attribute is System.Runtime.Serialization.DataMemberAttribute)
                    {
                        var propertyValue = property.GetValue(contract, null);
                        var propertyValueArray = propertyValue as byte[];
                        string displayValue;
                        if (propertyValueArray != null)
                            displayValue = "[## Binary data ##]";
                        else if (propertyValue == null)
                            displayValue = "** null **";
                        else
                            displayValue = propertyValue.ToString();
                        var newNode = parentNode.Nodes.Add(property.Name + ":  " + displayValue);
                        var link = new PropertyLink(contract, property);
                        newNode.Tag = link;

                        var list = propertyValue as IEnumerable;
                        if (propertyValueArray == null && !(propertyValue is string) && list != null)
                        {
                            var counter = -1;
                            foreach (var listObject in list)
                            {
                                counter++;
                                var newNode2 = newNode.Nodes.Add("[" + counter.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
                                ShowObject(listObject, newNode2);
                            }
                        }
                        else
                        {
                            if (propertyValueArray == null)
                                ShowObject(propertyValue, newNode);
                        }
                    }
            }
        }
    }

    /// <summary>
    /// Property link
    /// </summary>
    public class PropertyLink
    {
        /// <summary>
        /// Gets or sets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public object Instance { get; set; }

        /// <summary>
        /// Gets or sets the property info.
        /// </summary>
        /// <value>The property info.</value>
        public PropertyInfo PropertyInfo { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyLink"/> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="propertyInfo">The property info.</param>
        public PropertyLink(object instance, PropertyInfo propertyInfo)
        {
            Instance = instance;
            PropertyInfo = propertyInfo;
        }
    }
}
