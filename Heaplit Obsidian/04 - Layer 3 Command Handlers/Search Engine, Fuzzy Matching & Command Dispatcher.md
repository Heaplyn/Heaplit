---
title: "🔍 Search Engine, Fuzzy Matching & Command Dispatcher"
tags: ['search', 'fuzzy', 'levenshtein', 'searchutil', 'algorithms', 'deep-dive']
updated: 2026-09-05
vault_version: "5.0-MASTER-ENTERPRISE"
document_tier: "Pillar Master Guide (10+ Pages)"
status: verified-exhaustive
---

# 🔍 Search Engine, Fuzzy Matching & Command Dispatcher

## 🔍 High-Speed Fuzzy Search & Ranking Engine

`SearchUtil` (`Modules/Layer0/Common/SearchUtil.cs`) computes similarity scores between user search queries and command triggers in sub-millisecond execution time.

```mermaid
graph TD
    Query["User Input (e.g. 'sys', 'op pc', 'git com')"] --> Norm["Normalize & Lowercase String"]
    Norm --> MatchExact{"Exact Trigger Match?"}
    MatchExact -- Yes --> Score100["Score = 1.0 (Maximum Priority)"]
    MatchExact -- No --> Prefix{"Prefix or Acronym Match?"}
    Prefix -- Yes --> Score90["Score = 0.85 - 0.95"]
    Prefix -- No --> Lev["Levenshtein Distance Calculation"]
    Lev --> NormDist["Normalized Distance Similarity (0.0 to 1.0)"]
    NormDist --> Rank["Rank & Sort Suggestions"]
    Rank --> Render["Present Top Suggestions in Search Overlay"]
```

---

## 🧮 Levenshtein Distance & Word Boundary Algorithm
$$\text{Similarity}(S_1, S_2) = 1.0 - \frac{\text{LevenshteinDistance}(S_1, S_2)}{\max(\text{len}(S_1), \text{len}(S_2))}$$
Heaplit adds bonus weighting when search tokens match the beginnings of distinct words (e.g. `gco` matching `git checkout`).
