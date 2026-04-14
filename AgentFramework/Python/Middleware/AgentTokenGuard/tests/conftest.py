import sys
import os

# Ensure the token_guard_middleware package is importable when pytest runs from
# inside the tests/ directory. Adds the AgentTokenGuard root to sys.path so
# that `from token_guard_middleware import ...` resolves correctly.
_pkg_root = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
if _pkg_root not in sys.path:
    sys.path.insert(0, _pkg_root)
