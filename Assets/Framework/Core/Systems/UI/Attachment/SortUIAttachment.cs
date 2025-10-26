using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Core
{
    public class SortUIAttachment : UIAttachment
    {
        static List<IBaseUI> _uis = new List<IBaseUI>();

        readonly Dictionary<UIType, int> _typeMap = new Dictionary<UIType, int>
        {
            {
                UIType.Main, 0
            },
            {
                UIType.Popup, 5000
            },
            {
                UIType.Effect, 10000
            },
            {
                UIType.Top, 20000
            }
        };

        protected override Task OnBeforeShow(PipelineContext context)
        {
            if (!_uis.Contains(Target))
            {
                _uis.Add(Target);
            }

            var allIndex = _uis.Select(v => v.GetIndex()).ToList();

            _uis.Sort((cur, tar) =>
            {
                var curIndex = (cur == Target ? 1000 : cur.GetIndex()) + _typeMap[cur.UIType];
                var tarIndex = (tar == Target ? 1000 : tar.GetIndex()) + _typeMap[tar.UIType];

                return curIndex - tarIndex;
            });
            _uis.Each((v, i) =>

            {
                v.SetIndex(allIndex[i]);
            });
            return base.OnBeforeShow(context);
        }

    }
}