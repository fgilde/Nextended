
using System;
using System.Linq.Expressions;

namespace Nextended.Core.Extensions
{
	public static partial class FuncExtensions
	{
	
		public static Expression<Action<TParam1>> ToExpression<TParam1>(this Action<TParam1> f)
		{
			return (p1) => f(p1);
		}

		public static Expression<Action<TParam1,TParam2>> ToExpression<TParam1,TParam2>(this Action<TParam1,TParam2> f)
		{
			return (p1,p2) => f(p1,p2);
		}

		public static Expression<Action<TParam1,TParam2,TParam3>> ToExpression<TParam1,TParam2,TParam3>(this Action<TParam1,TParam2,TParam3> f)
		{
			return (p1,p2,p3) => f(p1,p2,p3);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4>> ToExpression<TParam1,TParam2,TParam3,TParam4>(this Action<TParam1,TParam2,TParam3,TParam4> f)
		{
			return (p1,p2,p3,p4) => f(p1,p2,p3,p4);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5> f)
		{
			return (p1,p2,p3,p4,p5) => f(p1,p2,p3,p4,p5);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6> f)
		{
			return (p1,p2,p3,p4,p5,p6) => f(p1,p2,p3,p4,p5,p6);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7) => f(p1,p2,p3,p4,p5,p6,p7);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8) => f(p1,p2,p3,p4,p5,p6,p7,p8);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15);
		}

		public static Expression<Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TParam16>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TParam16>(this Action<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TParam16> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15,p16) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15,p16);
		}


		public static Expression<Func<TParam1,TResult>> ToExpression<TParam1,TResult>(this Func<TParam1,TResult> f)
		{
			return (p1) => f(p1);
		}

		public static Expression<Func<TParam1,TParam2,TResult>> ToExpression<TParam1,TParam2,TResult>(this Func<TParam1,TParam2,TResult> f)
		{
			return (p1,p2) => f(p1,p2);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TResult>> ToExpression<TParam1,TParam2,TParam3,TResult>(this Func<TParam1,TParam2,TParam3,TResult> f)
		{
			return (p1,p2,p3) => f(p1,p2,p3);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TResult> f)
		{
			return (p1,p2,p3,p4) => f(p1,p2,p3,p4);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TResult> f)
		{
			return (p1,p2,p3,p4,p5) => f(p1,p2,p3,p4,p5);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6) => f(p1,p2,p3,p4,p5,p6);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7) => f(p1,p2,p3,p4,p5,p6,p7);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8) => f(p1,p2,p3,p4,p5,p6,p7,p8);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15);
		}

		public static Expression<Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TParam16,TResult>> ToExpression<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TParam16,TResult>(this Func<TParam1,TParam2,TParam3,TParam4,TParam5,TParam6,TParam7,TParam8,TParam9,TParam10,TParam11,TParam12,TParam13,TParam14,TParam15,TParam16,TResult> f)
		{
			return (p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15,p16) => f(p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15,p16);
		}

	} 
}
