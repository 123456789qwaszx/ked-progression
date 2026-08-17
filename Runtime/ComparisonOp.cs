namespace Ked.Progression
{
    /// <summary>
    /// 진행 조건의 비교 연산.
    ///
    /// 저작 쪽은 이 값을 <b>이름 문자열</b>로 내보낸다(runtime-contract §G1) —
    /// 순서를 재배열해도 데이터가 깨지지 않는다. 대신 모르는 이름이 들어올 수 있으므로,
    /// 문자열 → 이 enum 변환은 로더가 하고 실패는 진단으로 남긴다(규율 1: 침묵 금지).
    ///
    /// ⚠ <c>NotEqual</c>은 <b>일부러 없다</b>.
    /// 저작 쪽 파서가 아직 닫아 두어 데이터로 나오지 않는다(§G3).
    /// 미리 넣으면 평가기에 영원히 실행되지 않는 분기가 생긴다. 파서가 열리면 그때 더한다.
    /// </summary>
    public enum ComparisonOp
    {
        /// <summary>스탯 값이 기준 이상.</summary>
        GreaterOrEqual = 0,

        /// <summary>스탯 값이 기준 이하.</summary>
        LessOrEqual = 1,

        /// <summary>스탯 값이 기준과 같다. bool 스탯(0/1)이 쓰는 유일한 연산이다(§G4).</summary>
        Equal = 2,

        /// <summary>대상이 존재한다. 값 비교를 하지 않는다.</summary>
        Exists = 3,

        /// <summary>스탯 값이 기준 초과. 2026-08-16 저작 쪽 개방으로 추가(§G3).</summary>
        GreaterThan = 4,

        /// <summary>스탯 값이 기준 미만. 2026-08-16 저작 쪽 개방으로 추가(§G3).</summary>
        LessThan = 5,
    }
}
