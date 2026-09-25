"""Insiyab for Jinja: the macros, the library's files, and the setup for them.

    import insiyab
    insiyab.register(env)                 # any jinja2.Environment
    insiyab.init_app(app)                 # Flask: the macros and the files

    {% import "insiyab/ui.html" as ins %}
    {{ ins.head(plugins=['phone']) }}
    {{ ins.button('حفظ', variant='primary') }}

The macros are thin covers over Insiyab's classes and attributes: each writes the
markup the library's docs show, and insiyab.js, loaded by head(), does the rest.
"""
from __future__ import annotations

import os
import warnings
from typing import Any, Optional

__version__ = "0.6.0"
__all__ = ["register", "loader", "templates_dir", "static_dir", "init_app", "__version__"]

_HERE = os.path.dirname(os.path.abspath(__file__))
STATIC_URL = "/static/insiyab"


def templates_dir() -> str:
    """The folder holding insiyab/ui.html and the other macro files."""
    return os.path.join(_HERE, "templates")


def static_dir() -> Optional[str]:
    """The library's own files: insiyab.css, insiyab.js, fonts/ and plugins/.

    A wheel carries them in the package. A checkout of the repository has none there,
    so its dist/ is used instead. None when neither is found.
    """
    own = os.path.join(_HERE, "static")
    if os.path.isfile(os.path.join(own, "insiyab.js")):
        return own
    repo = os.path.normpath(os.path.join(_HERE, "..", "..", "..", "..", "dist"))
    if os.path.isfile(os.path.join(repo, "insiyab.js")):
        return repo
    return None


def loader() -> Any:
    """A loader for the macros alone, for an environment you put together yourself."""
    from jinja2 import FileSystemLoader

    return FileSystemLoader(templates_dir())


def register(env: Any, static_url: str = STATIC_URL) -> Any:
    """Make the macros importable in `env`, and tell head() where the files are served.

    The macros are added after the environment's own loader, so a template of the
    page's named insiyab/… still wins. Returns the environment.
    """
    from jinja2 import ChoiceLoader

    ours = loader()
    current = env.loader
    if current is None:
        env.loader = ours
    elif not _has_ours(current):
        env.loader = ChoiceLoader([current, ours])
    env.globals["insiyab_static"] = static_url
    if env.autoescape is False:
        warnings.warn(
            "insiyab: this Jinja environment does not autoescape, so text given to the "
            "macros is written as it is. Create it with autoescape=True (Flask, FastAPI "
            "and Django's Jinja backend already do).",
            stacklevel=2,
        )
    return env


def _has_ours(loader_: Any) -> bool:
    paths = getattr(loader_, "searchpath", None)
    if paths and os.path.normpath(templates_dir()) in [os.path.normpath(p) for p in paths]:
        return True
    return any(_has_ours(l) for l in getattr(loader_, "loaders", []))


def init_app(app: Any, url_prefix: str = STATIC_URL, name: str = "insiyab") -> Any:
    """Flask: serve the library's files under `url_prefix` and make the macros importable.

    One blueprint does both, so Flask's own template lookup finds insiyab/ui.html. Its
    .html name is what makes Flask autoescape it. Returns the blueprint.
    """
    from flask import Blueprint

    folder = static_dir()
    if folder is None:
        raise RuntimeError("insiyab: the library's files were not found; reinstall the package.")
    bp = Blueprint(name, __name__, template_folder=templates_dir(), static_folder=folder, static_url_path=url_prefix)
    app.register_blueprint(bp)
    app.jinja_env.globals["insiyab_static"] = url_prefix
    return bp
