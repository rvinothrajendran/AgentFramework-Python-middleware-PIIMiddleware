import sys
import os

# Ensure the installed language_middleware package (inner directory) takes
# precedence over the outer distribution folder of the same name, which pytest
# would otherwise discover as a namespace package when it adds the Lanuage/
# root to sys.path.
_pkg_src = os.path.join(os.path.dirname(__file__), "language_middleware")
if _pkg_src not in sys.path:
    sys.path.insert(0, _pkg_src)
