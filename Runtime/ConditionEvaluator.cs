using System;

namespace Ked.Progression
{
    /// <summary>
    /// 조건 판정. 순수 함수다 — 상태를 읽기만 한다(규율 4).
    ///
    /// ⚠ 여기서 판정하는 값은 <b>커밋 전 값</b>이어야 한다(§G6).
    /// 플레이어가 선택지를 보는 시점의 값이 기준이다.
    /// </summary>
    public static class ConditionEvaluator
    {
        public static bool IsMet(in ProgressionCondition condition, ProgressionState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            switch (condition.Kind)
            {
                case ConditionKind.EpisodeCleared:
                    // 연산은 볼 필요가 없다 — 팩토리가 Exists 말고는 만들 수 없게 한다.
                    // 구 런타임은 여기서 Op를 switch하다가 Equal을 exists로 뭉개 반대로 돌았다.
                    return state.IsEpisodeCleared(condition.Key);

                case ConditionKind.ChapterCleared:
                    return state.IsChapterCleared(condition.Key);

                case ConditionKind.Stat:
                    return EvaluateStat(condition, state);

                default:
                    // enum이 늘었는데 여기를 안 고친 경우. 조용히 false를 주면
                    // 새 조건 종류가 "언제나 미달"로 취급되어 관문이 통째로 잠긴다.
                    throw new NotSupportedException(
                        $"처리되지 않은 조건 종류 '{condition.Kind}'.");
            }
        }

        private static bool EvaluateStat(
            in ProgressionCondition condition, ProgressionState state)
        {
            // 정의되지 않은 키면 GetStat이 던진다 — 규율 1.
            int value = state.GetStat(condition.Key);

            switch (condition.Op)
            {
                case ComparisonOp.GreaterOrEqual: return value >= condition.Value;
                case ComparisonOp.LessOrEqual:    return value <= condition.Value;
                case ComparisonOp.Equal:          return value == condition.Value;
                case ComparisonOp.GreaterThan:    return value > condition.Value;
                case ComparisonOp.LessThan:       return value < condition.Value;

                case ComparisonOp.Exists:
                    // 여기 도달했다는 것은 스탯이 정의되어 있다는 뜻이다(GetStat이 통과했으므로).
                    // 다만 실제 데이터에 Stat+Exists가 나오는지 확인된 바 없다 — §5 참조.
                    return true;

                default:
                    throw new NotSupportedException(
                        $"처리되지 않은 비교 연산 '{condition.Op}'.");
            }
        }
    }
}
