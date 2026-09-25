/* The behaviour app: the stateful components wired to React state, with an
   <output> echoing each value, so the test can press a real key or click and read
   back what React now thinks. It is rendered on the server and hydrated, and also
   rendered from nothing, to cover both ways an app starts. */
import { useState } from 'react';
import {
  Button, Carousel, Check, ColorField, Combo, CommandPalette, DateField, Dialog, Field, FileField, Form, Menu, MenuCheckbox, MenuItem,
  Navbar, NavbarLink, Otp, Pagination, PhoneField, RelativeTime, Seg, Shell, Sidebar, SidebarBrand, SidebarGroup, SidebarLink, Tab, TabList,
  TabPanel, Tabs, ThemeToggle, Timeline, TimelineItem, Toc, ToggleButton, Topbar, TopbarTitle, Tree, Wizard, WizardPanel, Input, useTheme,
  SidebarToggle
} from '../src/index.js';

const cities = [
  { value: 'ruh', label: 'الرياض' },
  { value: 'jed', label: 'جدّة' },
  { value: 'alx', label: 'الإسكندريّة' }
];

export function App() {
  const [tab, setTab] = useState('a');
  const [seg, setSeg] = useState('day');
  const [pressed, setPressed] = useState(false);
  const [date, setDate] = useState('2026-09-30');
  const [city, setCity] = useState('');
  const [open, setOpen] = useState(false);
  const [dialogLog, setDialogLog] = useState('');
  const [archived, setArchived] = useState(true);
  const [step, setStep] = useState(0);
  const [phone, setPhone] = useState('');
  const [otp, setOtp] = useState('');
  const [color, setColor] = useState('#0f766e');
  const [menuOpen, setMenuOpen] = useState(false);
  const [page, setPage] = useState(1);
  const [slide, setSlide] = useState(0);
  const [show, setShow] = useState(true);
  const [treeLog, setTreeLog] = useState('');
  const [current, setCurrent] = useState('home');
  const [theme] = useTheme();

  return (
    <Shell
      sidebar={
        <Sidebar label="الأقسام" brand={<SidebarBrand href="#" mark="ان" name="انسياب" sub="تجربة" />}>
          <SidebarGroup label="عام">
            <SidebarLink id="nav-home" href="#home" current={current === 'home'} onClick={(e) => { e.preventDefault(); setCurrent('home'); }}>الرئيسية</SidebarLink>
            <SidebarLink id="nav-orders" href="#orders" current={current === 'orders'} onClick={(e) => { e.preventDefault(); setCurrent('orders'); }}>الطلبات</SidebarLink>
          </SidebarGroup>
        </Sidebar>
      }
      topbar={
        <Topbar
          start={<SidebarToggle />}
          center={<TopbarTitle title="تجربة" />}
          end={<ThemeToggle id="theme" className="ins-island ins-island--icon" aria-label="الوضع">☾</ThemeToggle>}
        />
      }
    >
      <output id="o-theme">{theme ?? ''}</output>

      <Tabs value={tab} onValueChange={setTab}>
        <TabList label="أقسام">
          <Tab value="a">أ</Tab>
          <Tab value="b">ب</Tab>
          <Tab value="c">ج</Tab>
        </TabList>
        <TabPanel value="a">لوحة أ</TabPanel>
        <TabPanel value="b">لوحة ب</TabPanel>
        <TabPanel value="c">لوحة ج</TabPanel>
      </Tabs>
      <output id="o-tab">{tab}</output>

      <Seg id="seg" label="الفترة" value={seg} onValueChange={setSeg} options={[{ value: 'day', label: 'يوم' }, { value: 'week', label: 'أسبوع' }]} />
      <output id="o-seg">{seg}</output>
      <Seg id="seg-fixed" label="ثابت" value="x" onValueChange={() => {}} options={[{ value: 'x', label: 'س' }, { value: 'y', label: 'ص' }]} />

      <ToggleButton id="toggle" pressed={pressed} onPressedChange={setPressed}>مفضّل</ToggleButton>
      <output id="o-toggle">{String(pressed)}</output>

      <Field label="التسليم" controlId="date">
        <DateField name="delivery" value={date} onValueChange={(iso) => setDate(iso)} />
      </Field>
      <output id="o-date">{date}</output>
      <Button id="date-set" onClick={() => setDate('2026-10-05')}>٥ أكتوبر</Button>

      <Field label="المدينة" controlId="city">
        <Combo name="city" options={cities} emptyText="لا شيء" onValueChange={(v) => setCity(v)} />
      </Field>
      <output id="o-city">{city}</output>

      <Button id="dialog-open" onClick={() => setOpen(true)}>افتح</Button>
      <Dialog id="dlg" open={open} onOpenChange={(o) => { setOpen(o); setDialogLog((l) => l + (o ? 'o' : 'c')); }} title="حوار" footer={<Button id="dlg-ok" dismiss>تمّ</Button>}>
        <p>نصّ</p>
      </Dialog>
      <output id="o-dialog">{dialogLog}</output>

      <Menu id="menu" label="قائمة" onOpenChange={setMenuOpen}>
        <MenuCheckbox id="menu-archived" checked={archived} onCheckedChange={setArchived}>المؤرشف</MenuCheckbox>
        <MenuItem id="menu-export">تصدير</MenuItem>
      </Menu>
      <output id="o-menu">{String(menuOpen) + '/' + String(archived)}</output>

      <Wizard id="wiz" steps={['أ', 'ب']} step={step} onStepChange={setStep} finishLabel="إنهاء" onSubmit={(e) => e.preventDefault()}>
        <WizardPanel label="أ">
          <Field required label="الاسم" controlId="wiz-name">
            <Input />
          </Field>
        </WizardPanel>
        <WizardPanel label="ب">
          <Check label="موافق" />
        </WizardPanel>
      </Wizard>
      <output id="o-step">{step}</output>

      {show && (
        <div id="removable">
          <Field label="الجوال" controlId="phone">
            <PhoneField name="mobile" defaultValue="+966501234567" onValueChange={(v) => setPhone(v.value)} />
          </Field>
          <Field label="الرمز" controlId="otp">
            <Otp length={4} name="code" onComplete={setOtp} />
          </Field>
        </div>
      )}
      <form id="phone-form">{show && <PhoneField id="phone2" name="second" defaultValue="+971501234567" />}</form>
      <output id="o-phone">{phone}</output>
      <output id="o-otp">{otp}</output>
      <Button id="toggle-show" onClick={() => setShow((s) => !s)}>إظهار/إخفاء</Button>

      <Field label="اللون" controlId="color">
        <ColorField name="brand" value={color} onValueChange={setColor} />
      </Field>
      <output id="o-color">{color}</output>
      <Button id="color-set" onClick={() => setColor('#1d4ed8')}>أزرق</Button>

      <Form id="file-form">
        <FileField id="files" name="docs" multiple />
      </Form>

      <Tree
        id="tree"
        label="شجرة"
        onAction={(d) => setTreeLog((l) => l + d.action[0])}
        items={[{ id: 't-a', label: 'أ', children: [{ id: 't-a1', label: 'أ١' }] }, { id: 't-b', label: 'ب' }]}
      />
      <output id="o-tree">{treeLog}</output>

      <Carousel id="car" label="شرائح" onIndexChange={setSlide}>
        <div>١</div>
        <div>٢</div>
        <div>٣</div>
      </Carousel>
      <output id="o-slide">{slide}</output>

      <Navbar id="navbar" brand="الموقع" label="الموقع">
        <NavbarLink href="#x" current>الرئيسية</NavbarLink>
      </Navbar>

      <Pagination id="pages" page={page} count={5} onPageChange={setPage} label="صفحات" />
      <output id="o-page">{page}</output>

      <Timeline>
        <TimelineItem title="حدث" time={<RelativeTime id="rel" dateTime="2020-01-02T10:00">٢ يناير</RelativeTime>} />
      </Timeline>

      <Toc id="toc" label="المحتوى" links={[{ href: '#removable', label: 'الحقول' }]} />

      <CommandPalette id="pal" label="لوحة" keys="none" items={[{ label: 'أمر تجريبي', key: 'demo', run: () => setTab('c') }]} />
    </Shell>
  );
}
