# IINACT_KR

marzent/IINACT를 포크해서 한국 서버용으로 수정한 프로젝트.

## 구조

- **IINACT_KR** (이 레포): `origin` = hareeen/IINACT_KR
- **machina** (서브모듈): hareeen/machina 포크, `dalamud` 브랜치 사용
  - remote `origin` = ravahn/machina (원본)
  - remote `marzent` = marzent/machina (포크)
  - remote `mine` = hareeen/machina (내 포크)

## 업스트림 동기화 가이드

### ravahn/machina가 업데이트된 경우 (주로 opcode)

```bash
cd machina
git fetch origin
git merge origin/master
git push mine dalamud
cd ..
git add machina
git commit -m "chore: update machina submodule"
git push origin main
```

### marzent/machina가 업데이트된 경우 (dalamud 관련)

```bash
cd machina
git fetch marzent
git merge marzent/dalamud
git push mine dalamud
cd ..
git add machina
git commit -m "chore: update machina submodule"
git push origin main
```

### marzent/IINACT (본체)가 업데이트된 경우

```bash
# upstream remote 추가 (최초 1회)
git remote add upstream https://github.com/marzent/IINACT

git fetch upstream
git merge upstream/main
git push origin main
```

## 한국 서버 관련 주요 변경점

- `Plugin.cs`: GameRegion을 Korean으로 하드코딩 + RegionLocked
- `machina/Machina.FFXIV/Headers/Opcodes/Korean.txt`: 한국 opcode 값
- `machina/Machina.FFXIV/Headers/Opcodes/OpcodeManager.cs`: RegionLocked 기능
- `OverlayPlugin.Core/resources/opcodes.jsonc`: Korean 리전 opcode
- 충돌 시 Korean 관련 값만 잘 유지하면 됨
