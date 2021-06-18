using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Nextended.UI.Helper;

namespace Nextended.UI.Classes
{
    /// <summary>
    /// PropertyGridTypeEditor
    /// </summary>
    public class PropertyGridTypeEditor : UITypeEditor
    {
        /// <summary>
        /// Gets the editor style used by the <see cref="M:System.Drawing.Design.UITypeEditor.EditValue(System.IServiceProvider,System.Object)"/> method.
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        /// <summary>
        /// Edits the specified object's value using the editor style indicated by the <see cref="M:System.Drawing.Design.UITypeEditor.GetEditStyle"/> method.
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var service = provider.GetService(typeof (IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (service == null || value == null)
                return value;

            Control control;
            var form = new Form { StartPosition = FormStartPosition.CenterParent, Text = value.ToString(), Width = 600, Height = 400};
            if (value is string || value.GetType().IsValueType)
            {
                control = new TextBox {Multiline = true, ReadOnly = true, Text = value.ToString(), Dock = DockStyle.Fill};
                form.Height = 200;
                form.Width = 300;
            }
			else if(value is IEnumerable)
            {
            	var editor = new CollectionEditor(value.GetType());
            	return editor.EditValue(context, provider, value);
            }
            else
            {
                control = new PropertyGrid {SelectedObject = new CustomClass(value), Dock = DockStyle.Fill};
				((PropertyGrid) control).ExtendSearch();
            }

            form.Controls.Add(control);
            service.ShowDialog(form);
            return value;
        }
    }
}