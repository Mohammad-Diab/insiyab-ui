"""The Python side of the Jinja package: setup, escaping, the head() tags, the files,
and Flask. Run from the repository root with `node test/run.mjs jinja`, or alone:

    python -m unittest discover -s packages/jinja/tests -v
"""
import json
import os
import sys
import unittest
import warnings

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(HERE, "..", "src"))

from jinja2 import DictLoader, Environment  # noqa: E402

import insiyab  # noqa: E402


def env(**kw):
    return insiyab.register(Environment(loader=DictLoader({}), autoescape=True), **kw)


def render(source, e=None):
    return (e or env()).from_string('{% import "insiyab/ui.html" as ins %}' + source).render().strip()


class Setup(unittest.TestCase):
    def test_version_matches_the_library(self):
        with open(os.path.join(ROOT, "package.json"), encoding="utf8") as f:
            self.assertEqual(insiyab.__version__, json.load(f)["version"])
        with open(os.path.join(HERE, "..", "pyproject.toml"), encoding="utf8") as f:
            self.assertIn('version = "%s"' % insiyab.__version__, f.read())

    def test_register_keeps_the_page_loader_first(self):
        e = Environment(loader=DictLoader({"insiyab/ui.html": "the page's own"}), autoescape=True)
        insiyab.register(e)
        self.assertEqual(e.get_template("insiyab/ui.html").render(), "the page's own")
        self.assertIn("ins-btn", e.get_template("insiyab/buttons.html").module.button("x"))

    def test_register_twice_adds_the_macros_once(self):
        e = env()
        insiyab.register(e)
        self.assertEqual(len(e.loader.loaders), 2)

    def test_register_on_an_environment_without_a_loader(self):
        e = insiyab.register(Environment(autoescape=True))
        self.assertIn("ins-pill", e.from_string('{% import "insiyab/ui.html" as ins %}{{ ins.pill("x") }}').render())

    def test_register_warns_without_autoescape(self):
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            insiyab.register(Environment())
        self.assertTrue(any("autoescape" in str(w.message) for w in caught))

    def test_every_group_imports_on_its_own(self):
        e = env()
        for name in os.listdir(os.path.join(insiyab.templates_dir(), "insiyab")):
            e.get_template("insiyab/" + name).module  # compiles and runs

    def test_static_dir_holds_the_library(self):
        folder = insiyab.static_dir()
        self.assertIsNotNone(folder)
        for part in ["insiyab.css", "insiyab.js", "insiyab.min.css", "plugins/insiyab-phone.js", "fonts"]:
            self.assertTrue(os.path.exists(os.path.join(folder, part)), part)


class Markup(unittest.TestCase):
    def test_text_is_escaped(self):
        self.assertEqual(render("{{ ins.badge('<b>') }}"), '<span class="ins-badge">&lt;b&gt;</span>')

    def test_attribute_values_are_escaped(self):
        self.assertIn('aria-label="a&#34;&gt;"', render("{{ ins.button('x', aria_label='a\">') }}"))

    def test_nested_macros_are_not_escaped_twice(self):
        out = render("{% call ins.button(variant='primary') %}{{ ins.icon('i-check') }}حفظ{% endcall %}")
        self.assertEqual(out, '<button class="ins-btn ins-btn--primary"><svg class="ico"><use href="#i-check"/></svg>حفظ</button>')

    def test_keywords_become_attributes(self):
        out = render("{{ ins.input(data_ins_tip=true, aria_describedby='h', disabled=false, placeholder=none, id='x') }}")
        self.assertEqual(out, '<input class="ins-input" aria-describedby="h" data-ins-tip id="x">')

    def test_the_action_attributes(self):
        out = render("{{ ins.button('حذف', confirm='حذف؟', confirm_ok='نعم', toast='تم', toast_tone='ok', dismiss=true, dialog='#d', tip=true) }}")
        for a in ['data-ins-confirm="حذف؟"', 'data-ins-confirm-ok="نعم"', 'data-ins-toast="تم"', 'data-ins-tone="ok"', "data-ins-dismiss", 'data-ins-dialog="#d"', "data-ins-tip"]:
            self.assertIn(a, out)

    def test_no_tone_without_a_toast(self):
        self.assertNotIn("data-ins-tone", render("{{ ins.button('x', toast_tone='ok') }}"))

    def test_select_marks_the_chosen_option(self):
        out = render("{{ ins.select([('a', 'أ'), ('b', 'ب')], selected='b', name='s') }}")
        self.assertIn('<option value="b" selected>ب</option>', out)
        self.assertIn('<option value="a">أ</option>', out)

    def test_pagination_window(self):
        out = render("{{ ins.pagination(6, 20, href='/p/{n}') }}")
        pages = [p for p in out.split('class="ins-page"')[1:]]
        self.assertEqual(out.count("ins-page-gap"), 2)
        self.assertIn('aria-current="page">6<', out)
        self.assertIn('href="/p/5"', out)
        self.assertIn('href="/p/20"', out)
        self.assertEqual(len(pages), 5)


class Readme(unittest.TestCase):
    def test_the_readme_example_renders(self):
        with open(os.path.join(HERE, "..", "README.md"), encoding="utf8") as f:
            text = f.read()
        source = text.split("```jinja\n", 1)[1].split("```", 1)[0]
        out = env().from_string(source).render()
        for part in ['<script src="/static/insiyab/insiyab.js">', 'data-ins-phone', 'data-ins-date', 'name="delivery"', 'ins-btn--primary', 'for="mobile"']:
            self.assertIn(part, out)

    def test_a_group_imports_by_name(self):
        out = env().from_string('{% from "insiyab/forms.html" import field, input %}{% call field("الاسم", id="n") %}{{ input(id="n") }}{% endcall %}').render()
        self.assertIn('<input class="ins-input" id="n">', out)


class Head(unittest.TestCase):
    def test_the_core_and_the_plugins_in_order(self):
        out = render("{{ ins.head(plugins=['hijri', 'phone']) }}", env(static_url="/assets/ins/"))
        lines = [l.strip() for l in out.splitlines() if l.strip()]
        self.assertEqual(
            lines,
            [
                '<link rel="stylesheet" href="/assets/ins/insiyab.css">',
                '<link rel="stylesheet" href="/assets/ins/plugins/insiyab-phone.css">',
                '<script src="/assets/ins/insiyab.js"></script>',
                '<script src="/assets/ins/plugins/insiyab-hijri.js"></script>',
                '<script src="/assets/ins/plugins/insiyab-phone.js"></script>',
            ],
        )

    def test_a_base_of_its_own_and_minified_css(self):
        out = render("{{ ins.head(base='https://cdn.example.com/insiyab', min=true) }}")
        self.assertIn('href="https://cdn.example.com/insiyab/insiyab.min.css"', out)

    def test_every_named_plugin_exists(self):
        folder = insiyab.static_dir()
        for p in ["palette", "otp", "phone", "file", "scrollspy", "timeline", "tree", "color", "carousel"]:
            self.assertTrue(os.path.isfile(os.path.join(folder, "plugins", "insiyab-%s.css" % p)), p)
            self.assertTrue(os.path.isfile(os.path.join(folder, "plugins", "insiyab-%s.js" % p)), p)


try:
    import flask
except ImportError:  # pragma: no cover
    flask = None


@unittest.skipIf(flask is None, "Flask is not installed")
class Flask(unittest.TestCase):
    def setUp(self):
        self.app = flask.Flask(__name__)
        insiyab.init_app(self.app)

        @self.app.route("/")
        def page():
            return flask.render_template_string('{% import "insiyab/ui.html" as ins %}{{ ins.head() }}{{ ins.badge(text) }}', text="<i>")

        self.client = self.app.test_client()

    def test_serves_the_files(self):
        res = self.client.get("/static/insiyab/insiyab.js")
        self.assertEqual(res.status_code, 200)
        self.assertIn(b"Insiyab", res.data)
        res.close()

    def test_serves_the_fonts_and_plugins(self):
        for path in ["/static/insiyab/plugins/insiyab-tree.js"]:
            res = self.client.get(path)
            self.assertEqual(res.status_code, 200, path)
            res.close()

    def test_renders_with_escaping_and_the_right_urls(self):
        html = self.client.get("/").get_data(as_text=True)
        self.assertIn('<script src="/static/insiyab/insiyab.js"></script>', html)
        self.assertIn('<span class="ins-badge">&lt;i&gt;</span>', html)

    def test_a_template_file_named_html_autoescapes(self):
        with self.app.app_context():
            t = self.app.jinja_env.get_template("insiyab/data.html")
            self.assertIn("&lt;", t.module.badge("<"))


if __name__ == "__main__":
    unittest.main()
